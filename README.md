# XML-Invoice-Validator-and-Logger
Reads XML invoice files from a local folder, extracts tag values, compares them with database records, and generates a success file or error log based on validation results.
This project is designed to automate the process of reading and validating XML invoice files stored in a local project directory. It consists of an XMLFileReader module that reads each XML file, extracts key tag values (like invoice number, date, amount, etc.), and compares them with existing invoice records in a connected database.

If validation passes (i.e., all tag values match existing database records), the system generates a success log file confirming the validation.

If validation fails (e.g., missing or incorrect tag content, or invoice not found in DB), an error log file is created with detailed information about the mismatches or missing elements.

Features:
Read XML files from a designated local folder

Parse and extract invoice information from specific XML tags

Connect and query the database for invoice verification

Generate success.txt for valid invoices

Generate error.log with specific validation issues
