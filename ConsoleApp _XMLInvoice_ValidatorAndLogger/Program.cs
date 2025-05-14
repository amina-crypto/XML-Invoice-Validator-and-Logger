
using System;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Serilog;
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
                xmlProcessor.ErrorLogger.Information("Application started at {Time}", DateTime.Now);
                xmlProcessor.SuccessLogger.Information("Application started at {Time}", DateTime.Now);
                xmlProcessor.TechnicalSuccessLogger.Information("Application started. [Information] Application started.");
                xmlProcessor.TechnicalErrorLogger.Information("Application started. [Information] Application started.");

                if (!dbConnector.Connect())
                {
                    Console.WriteLine("Failed to connect to the database.");
                    xmlProcessor.ErrorLogger.Error("Failed to connect to database at {Time}", DateTime.Now);
                    xmlProcessor.TechnicalErrorLogger.Error("Failed to connect to database at {Time} [Error] Failed to connect to database.", DateTime.Now);
            
                    xmlProcessor.OperationSuccessful = false;
                    return;
                }

                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();

                if (!Directory.Exists(folderPath))
                {
                    xmlProcessor.ErrorLogger.Error("Invalid directory: {FolderPath} at {Time}", folderPath, DateTime.Now);
                    xmlProcessor.TechnicalErrorLogger.Error("Invalid directory: {FolderPath} at {Time} [Error] Invalid directory provided.", folderPath, DateTime.Now);
               
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("Invalid directory.");
                    return;
                }

                xmlProcessor.ErrorLogger.Information("Found {FileCount} XML files in directory: {FolderPath} at {Time}", Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly).Length, folderPath, DateTime.Now);
                xmlProcessor.SuccessLogger.Information("Found {FileCount} XML files in directory: {FolderPath} at {Time}", Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly).Length, folderPath, DateTime.Now);
                xmlProcessor.TechnicalSuccessLogger.Information("User provided folder path: {FolderPath} [Information] User provided folder path: \"{FolderPath}\"", folderPath);
                xmlProcessor.TechnicalErrorLogger.Information("User provided folder path: {FolderPath} [Information] User provided folder path: \"{FolderPath}\"", folderPath);

                string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly);
                if (xmlFiles.Length == 0)
                {
                    xmlProcessor.ErrorLogger.Error("No XML files found in directory: {FolderPath} at {Time}", folderPath, DateTime.Now);
                    xmlProcessor.TechnicalErrorLogger.Error("No XML files found in directory: {FolderPath} at {Time} [Error] No XML files found in directory.", folderPath, DateTime.Now);
            
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("No XML files found in the directory.");
                    return;
                }

                DateTime startTime = DateTime.Now;
                foreach (string filePath in xmlFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    xmlProcessor.ErrorLogger.Information("Processing file: {FileName} at {Time}", fileName, DateTime.Now);
                    xmlProcessor.SuccessLogger.Information("Processing file: {FileName} at {Time}", fileName, DateTime.Now);
                    xmlProcessor.TechnicalSuccessLogger.Information("User provided file name: {FileName} [Information] User provided file name: \"{FileName}\"", fileName);
                    xmlProcessor.TechnicalErrorLogger.Information("User provided file name: {FileName} [Information] User provided file name: \"{FileName}\"", fileName);
                    xmlProcessor.TechnicalSuccessLogger.Information("Constructed file path: {FilePath} [Information] Constructed file path: \"{FilePath}\"", filePath);
                    xmlProcessor.TechnicalErrorLogger.Information("Constructed file path: {FilePath} [Information] Constructed file path: \"{FilePath}\"", filePath);
                    xmlProcessor.TechnicalSuccessLogger.Information("Reading file: {FileName} [Information] Reading file: \"{FileName}\"", fileName);
                    xmlProcessor.TechnicalErrorLogger.Information("Reading file: {FileName} [Information] Reading file: \"{FileName}\"", fileName);

                    var (xmlSuccess, xmlData) = xmlProcessor.ReadXmlNodes(filePath, fileName);
                    if (!xmlSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to process file: {FileName} at {Time}", fileName, DateTime.Now);
                        xmlProcessor.TechnicalErrorLogger.Error("Failed to process file: {FileName} at {Time} [Error] Failed to process XML file.", fileName, DateTime.Now);
                        xmlProcessor.OperationSuccessful = false;
                        continue;
                    }
                  

                    var (dbSuccess, dbResult) = dbConnector.ExecuteQuery(xmlData);
                    xmlProcessor.TechnicalSuccessLogger.Information("Executing  query:\"", dbResult);
                    if (!dbSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to execute database query for file: {FileName} at {Time}", fileName, DateTime.Now);
                        xmlProcessor.TechnicalErrorLogger.Error("Failed to execute database query for file: {FileName} at {Time} [Error] Database query execution failed.", fileName, DateTime.Now);
            
                        xmlProcessor.OperationSuccessful = false;
                        continue;
                    }

                    xmlProcessor.SuccessLogger.Information("Successfully processed file: {FileName} at {Time}", fileName, DateTime.Now);
                }

                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;
                string result = xmlProcessor.OperationSuccessful ? "Success" : "Failure";
                xmlProcessor.WriteLogs(result, elapsed);
            }
            catch (Exception ex)
            {
                xmlProcessor.ErrorLogger.Fatal(ex, "Unexpected error in Main method at {Time}", DateTime.Now);
                xmlProcessor.TechnicalErrorLogger.Fatal("Unexpected error in Main method at {Time} [Error] Unexpected error executing application.", DateTime.Now);
                xmlProcessor.OperationSuccessful = false;
            }
            finally
            {
                xmlProcessor.ErrorLogger.Information("Application ending at {Time}", DateTime.Now);
                xmlProcessor.SuccessLogger.Information("Application ending at {Time}", DateTime.Now);
                xmlProcessor.TechnicalSuccessLogger.Information("Application ending. [Information] Application ending.");
                xmlProcessor.TechnicalErrorLogger.Information("Application ending. [Information] Application ending.");
            

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush();
            }
        }
    }
}