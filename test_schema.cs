using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backend", "Schemas", "TallyJv2-Export.xsd");
            var xmlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_sample.xml");

            var xmlDoc = new XmlDocument();

            using (var reader = new StreamReader(xmlPath))
            {
                var xmlContent = reader.ReadToEnd();
                xmlDoc.LoadXml(xmlContent);
            }

            // Validate against schema
            xmlDoc.Schemas.Add("urn:tallyj.bahai:v2", schemaPath);
            var validationErrors = new List<string>();
            xmlDoc.Validate((sender, args) =>
            {
                if (args.Severity == XmlSeverityType.Error)
                    validationErrors.Add(args.Message);
            });

            if (validationErrors.Any())
            {
                Console.WriteLine("XML validation failed:");
                foreach (var error in validationErrors)
                {
                    Console.WriteLine($"- {error}");
                }
                Console.WriteLine($"Total errors: {validationErrors.Count}");
            }
            else
            {
                Console.WriteLine("XML validation passed successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}