using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NDesk.Options;
using RallyIntegrator.Library;

namespace RallyIntegrator.ConsoleApplication
{
    class Program
    {
        const int PartitionSize = 10;
        private static readonly string Executable = Path.GetFileName(Assembly.GetExecutingAssembly().CodeBase);

        public static void Main(string[] args)
        {
            var showHelp = false;
            var revisions = new List<string>();

            var optionSet = new OptionSet {
                { "c|changeset=", "the {changesets} to be integrated.", c => revisions.Add (c.Trim(new []{' ', ',', ';'})) },
                { "h|help",  "show this message and exit", v => showHelp = v != null },
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("ri: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", Executable);
                return;
            }

            if (showHelp)
            {
                ShowHelp(optionSet);
                return;
            }
            if (revisions.Count > PartitionSize)
            {
                var parallelOptions = new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount - 1};
                Parallel.ForEach(revisions.Select(int.Parse).Partition(PartitionSize), parallelOptions, Integrator.Process);
            }
            else
                Integrator.Process(revisions.Select(int.Parse));

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("All done!");
            Console.ResetColor();
            Console.ReadKey();
        }

        static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: {0} [OPTIONS]", Executable);
            Console.WriteLine("Integrate a list of changesets with Rally");
            Console.WriteLine();
            Console.WriteLine("Options:");
            p.WriteOptionDescriptions(Console.Out);
        }
    }
}
