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

              
                string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly);
                if (xmlFiles.Length == 0)
                {
                    xmlProcessor.ErrorLogger.Error("No XML files found in directory: {FolderPath}", folderPath);
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("No XML files found in the directory.");
                    return;
                }

                xmlProcessor.ErrorLogger.Information("Found {FileCount} XML files in directory: {FolderPath}", xmlFiles.Length, folderPath);
                xmlProcessor.SuccessLogger.Information("Found {FileCount} XML files in directory: {FolderPath}", xmlFiles.Length, folderPath);

              
                foreach (string filePath in xmlFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    xmlProcessor.ErrorLogger.Information("Processing file: {FileName}", fileName);
                    xmlProcessor.SuccessLogger.Information("Processing file: {FileName}", fileName);

                    var (xmlSuccess, xmlData) = xmlProcessor.ReadXmlNodes(filePath, fileName);
                    if (!xmlSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to process file: {FileName}", fileName);
                        xmlProcessor.OperationSuccessful = false;
                        continue; // Continue with the next file even if this one fails
                    }

                    var (dbSuccess, dbResult) = dbConnector.ExecuteQuery(xmlData);
                    if (!dbSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to execute database query for file: {FileName}", fileName);
                        xmlProcessor.OperationSuccessful = false;
                        continue; 
                    }

                    xmlProcessor.SuccessLogger.Information("Successfully processed file: {FileName}", fileName);
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
                xmlProcessor.WriteLogs();

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush();
            }
        }
    }
}