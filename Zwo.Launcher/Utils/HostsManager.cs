using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zwo.Launcher.Pages;

namespace Zwo.Launcher.Utils
{
    static class HostsManager
    {
        public class HostsEntry(string ipAddress, List<string> domains)
        {
            public string IpAddress { get; set; } = ipAddress;
            public List<string> Domains { get; set; } = domains;

            public override string ToString()
            {
                return $"{IpAddress} {string.Join(" ", Domains)}";
            }

            public static HostsEntry Parse(string line)
            {
                var parts = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    return null;
                }

                var ipAddress = parts[0];
                var domains = parts.Skip(1).ToList();

                return new HostsEntry(ipAddress, domains);
            }

            public override bool Equals(object obj)
            {
                if (obj is HostsEntry other)
                {
                    return IpAddress == other.IpAddress && Domains.SequenceEqual(other.Domains);
                }
                return false;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(IpAddress, Domains);
            }
        }

        private static readonly string HostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

        private const string ProgramBlockStart = "# Begin Zwo.Launcher Controlled Block";
        private const string ProgramBlockEnd = "# End Zwo.Launcher Controlled Block";

        public static void AddEntry(HostsEntry entry)
        {
            var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();
            var programBlockIndex = FindProgramBlockIndex(hostsFileContent);

            if (programBlockIndex.Item1 == -1 || programBlockIndex.Item2 == -1)
            {
                hostsFileContent.Add(ProgramBlockStart);
                hostsFileContent.Add(entry.ToString());
                hostsFileContent.Add(ProgramBlockEnd);
            }
            else
            {
                hostsFileContent.Insert(programBlockIndex.Item2, entry.ToString());
            }

            File.WriteAllLines(HostsFilePath, hostsFileContent);
        }

        public static void RemoveEntry(HostsEntry entry)
        {
            var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();
            var programBlockIndex = FindProgramBlockIndex(hostsFileContent);

            if (programBlockIndex.Item1 != -1 && programBlockIndex.Item2 != -1)
            {
                hostsFileContent = hostsFileContent.Where((line, index) =>
                    index < programBlockIndex.Item1 ||
                    index > programBlockIndex.Item2 ||
                    !entry.Equals(HostsEntry.Parse(line))).ToList();

                File.WriteAllLines(HostsFilePath, hostsFileContent);
            }
        }

        public static List<HostsEntry> GetEntries()
        {
            var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();
            var programBlockIndex = FindProgramBlockIndex(hostsFileContent);
            var entries = new List<HostsEntry>();

            if (programBlockIndex.Item1 != -1 && programBlockIndex.Item2 != -1)
            {
                for (int i = programBlockIndex.Item1 + 1; i < programBlockIndex.Item2; i++)
                {
                    var entry = HostsEntry.Parse(hostsFileContent[i]);
                    if (entry != null)
                    {
                        entries.Add(entry);
                    }
                }
            }

            return entries;
        }

        public static void RemoveAllEntries()
        {
            var hostsFileContent = File.ReadAllLines(HostsFilePath).ToList();
            var programBlockIndex = FindProgramBlockIndex(hostsFileContent);

            if (programBlockIndex.Item1 != -1 && programBlockIndex.Item2 != -1)
            {
                hostsFileContent.RemoveRange(programBlockIndex.Item1, programBlockIndex.Item2 - programBlockIndex.Item1 + 1);
                File.WriteAllLines(HostsFilePath, hostsFileContent);
            }
        }

        private static (int, int) FindProgramBlockIndex(List<string> hostsFileContent)
        {
            int startIndex = hostsFileContent.IndexOf(ProgramBlockStart);
            int endIndex = hostsFileContent.IndexOf(ProgramBlockEnd);

            return (startIndex, endIndex);
        }
    }
}
