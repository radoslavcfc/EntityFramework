namespace Cinema.DataProcessor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using System.Xml;
    using System.Xml.Serialization;    
    using Newtonsoft.Json;

    using Data;
    using DataProcessor.ExportDto;
    
    public class Serializer
    {
        //Json export:
        public static string ExportTopMovies(CinemaContext context, int rating)
        {
            #region Query
            //. Export all movies which have rating more or equal to the given and have at least 
            //    one projection with sold tickets.For each movie, export its name, rating formatted 
            //    to the second digit, total incomes formatted same way and customers. For each customer,
            //    export its first name, last name and balance formatted to the second digit. Order the 
            //    customers by balance(descending), then by first name(ascending) and last name(ascending).
            //    Take first 10 records and order the movies by rating(descending), then by total incomes
            //    (descending).
            #endregion

            var sb = new StringBuilder();
            var collection = context.Movies
                .Where(m => m.Rating >= rating && m.Projections.Any(p => p.Tickets.Count >= 1))
                .OrderByDescending(m => m.Rating)
                .ThenByDescending(m => m.Projections.Sum(p => p.Tickets.Sum(t => t.Price)))
                .Take(10)

                .Select(m => new
                {
                    MovieName = m.Title,
                    Rating = $"{m.Rating:F2}",
                    TotalIncomes = $"{m.Projections.Sum(p => p.Tickets.Sum(t => t.Price)):F2}",
                    Customers = m.Projections
                       .SelectMany(pr => pr.Tickets)
                                       .Select(
                                            t => new
                                            {
                                                FirstName = t.Customer.FirstName,
                                                LastName = t.Customer.LastName,
                                                Ballance = (t.Customer.Balance).ToString("F2")
                                            })
                                            .OrderByDescending(anoType => anoType.Ballance)
                                            .ThenBy(anoType => anoType.FirstName)
                                            .ThenBy(anoType => anoType.LastName)
                                            .ToArray()
                })     
                .ToArray();

             var serializer = JsonConvert.SerializeObject(collection, Newtonsoft.Json.Formatting.Indented);

            return serializer;
        }

        //XML export: 
        public static string ExportTopCustomers(CinemaContext context, int age)
        {
            #region Query
            //Export customers with age above or equal to the given. For each customer, export their first name, 
            //last name, spent money for tickets(formatted to the second digit) and spent time
            //(in format: "hh\:mm\:ss").Take first 10 records and order the result by spent money in descending order.
            #endregion

            var customers = context.Customers
                .Where(c => c.Age >= age)
                .OrderByDescending(c => c.Tickets.Sum(t => t.Price))
                .Take(10)
                .Select(c => new CustomerExportDto
                {
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    SpentMoney = $"{c.Tickets.Sum(t => t.Price):F2}",
                    SpentTime = TimeSpan.FromSeconds(c.Tickets.Sum(t => t.Projection.Movie.Duration.TotalSeconds))
                    .ToString(@"hh\:mm\:ss")
                })
                .ToArray();

            var xmlSer = new XmlSerializer(typeof(CustomerExportDto[]), new XmlRootAttribute("Customers"));
            var sb = new StringBuilder();
            var namespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            xmlSer.Serialize(new StringWriter(sb), customers, namespaces);
            return sb.ToString().TrimEnd();
        }
    }
}