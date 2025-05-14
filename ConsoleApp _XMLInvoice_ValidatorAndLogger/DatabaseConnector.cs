using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Oracle.ManagedDataAccess.Client;

namespace ConsoleApp__XMLInvoice_ValidatorAndLogger
{
    public class DatabaseConnector : LoggableOperation
    {
        private readonly string ConnectionString =
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=whitenetx-db.randstaditaly.it)(PORT=1524))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=WN12PROD)));User Id=S2N;Password=g_Fb34gDSfqmfNs_2;";

        public bool Connect()
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    ErrorLogger.Information("Successfully connected to Oracle database.");
                    SuccessLogger.Information("Successfully connected to Oracle database.");
                    return true;
                }
            }
            catch (OracleException ex)
            {
                ErrorLogger.Error(ex, "Oracle database connection error: {ErrorMessage}", ex.Message);
                OperationSuccessful = false;
                return false;
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex, "Unexpected error connecting to database.");
                OperationSuccessful = false;
                return false;
            }
        }

        public (bool Success, InvoiceMatchDto Result) ExecuteQuery(Dictionary<string, string> xmlData)
        {
            try
            {
                string numero = xmlData["Numero"]?.Trim();
                string idCodiceCessionario = xmlData["IdCodice"]?.Trim();
                string capCessionario = xmlData["CAP"];
                string denominazione = xmlData["Denominazione"];
                string nazioneCessionario = xmlData["Nazione"];
                string indirizzoCessionario = xmlData["Indirizzo"];
                string comuneCessionario = xmlData["Comune"];
                string xmlFilePattern = xmlData["FileName"];
                string fullPattern = $"%{xmlFilePattern}%";
                string numeroLastThree = numero?.Length >= 3 ? numero.Substring(numero.Length - 3) : numero;
                int numeroParsed;
                if (string.IsNullOrEmpty(numeroLastThree) || !int.TryParse(numeroLastThree, out numeroParsed))
                {
                    ErrorLogger.Error("Invalid number format for Numero (last 3 digits): {Numero}", numeroLastThree);
                    OperationSuccessful = false;
                    return (false, null);
                }
                if (!decimal.TryParse(idCodiceCessionario, out decimal idCodiceParsed))
                {
                    ErrorLogger.Error("Invalid number format for IdCodice: {IdCodice}", idCodiceCessionario);
                    OperationSuccessful = false;
                    return (false, null);
                }

                var parameters = new
                {
                    numero = numeroParsed,
                    cap = capCessionario,
                    nazione = nazioneCessionario,
                    indirizzo = indirizzoCessionario,
                    comune = comuneCessionario,
                    idCodice = idCodiceParsed,
                    denominazione = denominazione,
                    xmlFilePattern = fullPattern
                };

                string query = @"
                    SELECT 
                        i.invoice_id,
                        CASE WHEN i.invoice_number = :numero THEN 1 ELSE 0 END AS Numero,
                        CASE WHEN i.zip_code_invo = :cap THEN 1 ELSE 0 END AS CAP,
                        CASE WHEN rc.iso_cod_invo = :nazione THEN 1 ELSE 0 END AS Nazione,
                        CASE WHEN i.address_legal = :indirizzo THEN 1 ELSE 0 END AS Indirizzo,
                        CASE WHEN i.des_municipality_invo = :comune THEN 1 ELSE 0 END AS Comune,
                        CASE WHEN i.company_name = :denominazione THEN 1 ELSE 0 END AS Denominazione,
                        CASE WHEN i.fiscal_identity_code = :idCodice THEN 1 ELSE 0 END AS IDCodice,
                        ish.send_data AS Folder_Name
                    FROM 
                        INVO_SEND_HISTORY ish
                        INNER JOIN INVOICE i ON ish.invoice_id = i.invoice_id
                        INNER JOIN REFE_COUNTRY rc ON rc.refe_country_id = i.refe_country_invo_id
                    WHERE 
                        ish.send_data LIKE :xmlFilePattern
                        AND i.invoice_id = i.invoice_id 
                        AND rc.refe_country_id = i.refe_country_invo_id";

                using (OracleConnection connection = new OracleConnection(ConnectionString))
                {
                    connection.Open();
                    try
                    {
                        var result = connection.Query<InvoiceMatchDto>(query, parameters).ToList().SingleOrDefault();
                        if (result == null)
                        {
                            ErrorLogger.Error("No matching record found in the database for the given criteria.");
                            OperationSuccessful = false;
                            return (false, null);
                        }


                        Console.WriteLine("\nQuery Results (Match Status):");
                        Console.WriteLine($"Numero Match: {(result.Numero == 1 ? "True" : "False")}");
                        Console.WriteLine($"CAP Match: {(result.CAP == 1 ? "True" : "False")}");
                        Console.WriteLine($"Nazione Match: {(result.Nazione == 1 ? "True" : "False")}");
                        Console.WriteLine($"Indirizzo Match: {(result.Indirizzo == 1 ? "True" : "False")}");
                        Console.WriteLine($"Comune Match: {(result.Comune == 1 ? "True" : "False")}");
                        Console.WriteLine($"Denominazione Match: {(result.Denominazione == 1 ? "True" : "False")}");
                        Console.WriteLine($"IDCodice Match: {(result.IDCodice == 1 ? "True" : "False")}");
                        Console.WriteLine($"Folder Name: {result.FolderName}");

                        return (true, result);
                    }
                    catch (OracleException ex)
                    {
                        ErrorLogger.Error(ex, "Oracle error during query execution: {ErrorMessage}", ex.Message);
                        OperationSuccessful = false;
                        return (false, null);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Error(ex, "Error executing database query.");
                OperationSuccessful = false;
                return (false, null);
            }
        }
    }
}
