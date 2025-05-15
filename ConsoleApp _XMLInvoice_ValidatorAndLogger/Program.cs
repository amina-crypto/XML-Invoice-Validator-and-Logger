
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
                xmlProcessor.ErrorLogger.Information("Application started ");
                xmlProcessor.SuccessLogger.Information("Application started ");
                xmlProcessor.TechnicalSuccessLogger.Information("Application started.");
                xmlProcessor.TechnicalErrorLogger.Information("Application started.");

                if (!dbConnector.Connect())
                {
                    Console.WriteLine("Failed to connect to the database.");
                    xmlProcessor.ErrorLogger.Error("Failed to connect to database ");
                    xmlProcessor.TechnicalErrorLogger.Error("Failed to connect to database.");
            
                    xmlProcessor.OperationSuccessful = false;
                    return;
                }

                Console.WriteLine("Enter the path to the folder containing XML files:");
                string folderPath = Console.ReadLine();

                if (!Directory.Exists(folderPath))
                {
                    xmlProcessor.ErrorLogger.Error("Invalid directory:{FolderPath}", folderPath);
                    xmlProcessor.TechnicalErrorLogger.Error("Invalid directory:{FolderPath} ", folderPath);
               
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("Invalid directory.");
                    return;
                }

                xmlProcessor.ErrorLogger.Information("Found {FileCount} XML files in directory:{FolderPath}", Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly).Length, folderPath);
                xmlProcessor.SuccessLogger.Information("Found {FileCount} XML files in directory:{FolderPath}", Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly).Length, folderPath);
                xmlProcessor.TechnicalSuccessLogger.Information("User provided folder path:{FolderPath}", folderPath);
                xmlProcessor.TechnicalErrorLogger.Information("User provided folder path:{FolderPath}", folderPath);

                string[] xmlFiles = Directory.GetFiles(folderPath, "*.xml", SearchOption.TopDirectoryOnly);
                if (xmlFiles.Length == 0)
                {
                    xmlProcessor.ErrorLogger.Error("No XML files found in directory:{FolderPath} ", folderPath);
                    xmlProcessor.TechnicalErrorLogger.Error("No XML files found in directory:{FolderPath}", folderPath);
            
                    xmlProcessor.OperationSuccessful = false;
                    Console.WriteLine("No XML files found in the directory.");
                    return;
                }

                DateTime startTime = DateTime.Now;
                foreach (string filePath in xmlFiles)
                {
                    string fileName = Path.GetFileName(filePath);
                    xmlProcessor.ErrorLogger.Information("Processing file:{FileName}", fileName);
                    xmlProcessor.SuccessLogger.Information("Processing file:{FileName}", fileName);
                    xmlProcessor.TechnicalSuccessLogger.Information("User provided file name:{FileName}", fileName);
                    xmlProcessor.TechnicalErrorLogger.Information("User provided file name: {FileName}", fileName);
                    xmlProcessor.TechnicalSuccessLogger.Information("Constructed file path:{FileName}", filePath);
                    xmlProcessor.TechnicalErrorLogger.Information("Constructed file path:{FileName} ", filePath);
                    xmlProcessor.TechnicalSuccessLogger.Information("Reading file:{FileName}", fileName);
                    xmlProcessor.TechnicalErrorLogger.Information("Reading file:{FileName}", fileName);

                    var (xmlSuccess, xmlData) = xmlProcessor.ReadXmlNodes(filePath, fileName);
                    if (!xmlSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to process file:{FileName}", fileName);
                        xmlProcessor.TechnicalErrorLogger.Error("Failed to process file:{FileName}", fileName);
                        xmlProcessor.OperationSuccessful = false;
                        continue;
                    }
                  

                    var (dbSuccess, dbResult) = dbConnector.ExecuteQuery(xmlData);
                    xmlProcessor.TechnicalSuccessLogger.Information("Query Results (Match Status):");
                    xmlProcessor.TechnicalSuccessLogger.Information("Numero Match: {Match}", dbResult.Numero == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("CAP Match: {Match}", dbResult.CAP == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("Nazione Match: {Match}", dbResult.Nazione == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("Indirizzo Match: {Match}", dbResult.Indirizzo == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("Comune Match: {Match}", dbResult.Comune == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("Denominazione Match: {Match}", dbResult.Denominazione == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("IDCodice Match: {Match}", dbResult.IDCodice == 1 ? "True" : "False");
                    xmlProcessor.TechnicalSuccessLogger.Information("FolderName: {FileName}", dbResult.FolderName);
                    xmlProcessor.TechnicalSuccessLogger.Information("\nResults of the next file ");
                    if (!dbSuccess)
                    {
                        xmlProcessor.ErrorLogger.Error("Failed to execute database query for file:{FileName} ", fileName);
                        xmlProcessor.TechnicalErrorLogger.Error("Failed to execute database query for file:{FileName}", fileName);
            
                        xmlProcessor.OperationSuccessful = false;
                        continue;
                    }

                   xmlProcessor.SuccessLogger.Information("Successfully processed file:{FileName}", fileName);
                }

                DateTime endTime = DateTime.Now;
                TimeSpan elapsed = endTime - startTime;
                string result = xmlProcessor.OperationSuccessful ? "Success" : "Failure";
                xmlProcessor.WriteLogs(result, elapsed);
            }
            catch (Exception ex)
            {
                xmlProcessor.ErrorLogger.Fatal(ex, "Unexpected error in Main method ");
                xmlProcessor.TechnicalErrorLogger.Fatal("Unexpected error in Main method .");
                xmlProcessor.OperationSuccessful = false;
            }
            finally
            {
                xmlProcessor.ErrorLogger.Information("Application ending .");
                xmlProcessor.SuccessLogger.Information("Application ending .");
                xmlProcessor.TechnicalSuccessLogger.Information("Application ending.");
                xmlProcessor.TechnicalErrorLogger.Information("Application ending.");
            

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                Log.CloseAndFlush();
            }
        }
    }
}