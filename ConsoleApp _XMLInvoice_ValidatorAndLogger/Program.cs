using System;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Generic;
using Dapper;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    class Program
    {
        static void Main(string[] args)
        {
            var xmlProcessor = new XmlProcessor();
            var dbConnector = new DatabaseConnector();

            try
            {
                xmlProcessor.ErrorLogger.Information("Application started.");
                xmlProcessor.SuccessLogger.Information("Application started.");

                if (!dbConnector.Connect())
                {
                    Console.WriteLine("Failed to connect to the database.");
                    xmlProcessor.OperationSuccessful = false;
                    return;
                }

                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();
                xmlProcessor.ErrorLogger.Information("User provided folder path: {FolderPath}", folderPath);
                xmlProcessor.SuccessLogger.Information("User provided folder path: {FolderPath}", folderPath);

                if (!Directory.Exists(folderPath))
                {
                    xmlProcessor.ErrorLogger.Error("Invalid directory: {FolderPath}", folderPath);
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("Invalid directory.");
                    return;
                }

                Console.WriteLine("Enter the XML file name (with or without .xml extension):");
                string fileName = Console.ReadLine();
                xmlProcessor.ErrorLogger.Information("User provided file name: {FileName}", fileName);
                xmlProcessor.SuccessLogger.Information("User provided file name: {FileName}", fileName);

                if (!fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".xml";
                }

                string filePath = Path.Combine(folderPath, fileName);
                xmlProcessor.ErrorLogger.Information("Constructed file path: {FilePath}", filePath);
                xmlProcessor.SuccessLogger.Information("Constructed file path: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    xmlProcessor.ErrorLogger.Error("File not found: {FilePath}", filePath);
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("File not found.");
                    return;
                }

                xmlProcessor.ErrorLogger.Information("Reading file: {FileName}", fileName);
                xmlProcessor.SuccessLogger.Information("Reading file: {FileName}", fileName);

                var (xmlSuccess, xmlData) = xmlProcessor.ReadXmlNodes(filePath, fileName);
                if (!xmlSuccess)
                {
                    xmlProcessor.OperationSuccessful = false;
                    return;
                }

                var (dbSuccess, dbResult) = dbConnector.ExecuteQuery(xmlData);
                if (!dbSuccess)
                {
                    xmlProcessor.OperationSuccessful = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                xmlProcessor.ErrorLogger.Fatal(ex, "Unexpected error in Main method.");
                xmlProcessor.OperationSuccessful = false;
            }
            finally
            {
                xmlProcessor.ErrorLogger.Information("Application ending.");
                xmlProcessor.SuccessLogger.Information("Application ending.");

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush();
            }
        }
    }
}