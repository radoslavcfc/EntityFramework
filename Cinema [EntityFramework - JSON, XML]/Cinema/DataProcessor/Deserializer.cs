namespace Cinema.DataProcessor
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;

    using AutoMapper;
    using Newtonsoft.Json;
    using System.Xml.Serialization;
    using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

    using Cinema.Data;
    using Cinema.Data.Models;
    using Cinema.Data.Models.Enums;
    using Cinema.DataProcessor.ImportDto;
    
    public class Deserializer
    {
        private const string ErrorMessage = "Invalid data!";
        private const string SuccessfulImportMovie 
            = "Successfully imported {0} with genre {1} and rating {2}!";
        private const string SuccessfulImportHallSeat 
            = "Successfully imported {0}({1}) with {2} seats!";
        private const string SuccessfulImportProjection 
            = "Successfully imported projection {0} on {1}!";
        private const string SuccessfulImportCustomerTicket 
            = "Successfully imported customer {0} {1} with bought tickets: {2}!";

        // I. Json import:
        public static string ImportMovies(CinemaContext context, string jsonString)
        {
            #region Query
            // Using the file movies.json, import the data from that file into the database.

            //  Constraints
            //•	If any validation errors occur(such as if Rating is not between 1 and 10, a Title/ Genre / Duration
            /// Rating / Director is missing, or they exceed required the min and max length),
            /// do not import any part of the entity and append an error message to the method output.
            //•	If a title already exists, do not import it and append an error message.
            #endregion

            var moviesDtoCollection = JsonConvert.DeserializeObject<MoviesImportDto[]>(jsonString);
            var stringBuilder = new StringBuilder();

            var collectionToUpload = new List<MoviesImportDto>();
            foreach (var movieDto in moviesDtoCollection)
            {
                var isValidMovie = IsValid(movieDto);
                var titleExists = collectionToUpload.Any(m => m.Title == movieDto.Title);
                var enumCheck = Enum.TryParse(typeof(Genre), movieDto.Genre.ToString(), out object genre);

                if (titleExists || !isValidMovie || !enumCheck)
                {
                    stringBuilder.AppendLine(ErrorMessage);
                    continue;
                }
                else
                {
                    collectionToUpload.Add(movieDto);
                    stringBuilder.AppendLine(
                    String.Format(SuccessfulImportMovie, 
                    movieDto.Title, movieDto.Genre, movieDto.Rating));
                }
            }

            var moviesCollection = Mapper.Map<Movie[]>(collectionToUpload);
            context.Movies.AddRange(moviesCollection);

            context.SaveChanges();
             
            return stringBuilder.ToString().TrimEnd();
        }

        public static string ImportHallSeats(CinemaContext context, string jsonString)
        {
            #region Query
            //Using the file halls-seats.json, import the data from that file into the database.
            
            //Constraints
            //     •	If any validation errors occur, such as invalid hall name, zero or negative seats count, 
            //         ignore the entity and print an error message.
            #endregion

            var collectionOfSeatsDto = JsonConvert.DeserializeObject<HallImportDto[]>(jsonString);
            var halls = new List<Hall>();

            var sb = new StringBuilder();

            foreach (var hallDto in collectionOfSeatsDto)
            {
                if (!IsValid(hallDto) || hallDto.Seats <= 0)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var hall = new Hall
                {
                    Name = hallDto.Name,
                    Is3D = hallDto.Is3D,
                    Is4Dx = hallDto.Is4Dx
                };

                for (int i = 0; i < hallDto.Seats; i++)
                {
                     hall.Seats.Add(new Seat());
                }

                halls.Add(hall);

                var status = "Normal";
                if (hall.Is4Dx)
                {
                    status = hall.Is3D ? "4DX/3D" : "4DX";
                }
                else if (hall.Is3D)
                {
                    status = "3D";
                }

                sb.AppendLine(String.Format(SuccessfulImportHallSeat, hall.Name, status, hall.Seats.Count));
            }
            context.Halls.AddRange(halls);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        // II. Xml import:
        public static string ImportProjections(CinemaContext context, string xmlString)
        {
            #region Query
            //Using the file projections.xml, import the data from the file into the database.
                       
            //Constraints
            //•	If there are any validation errors(such as invalid movie or hall), do not import any part 
            //  of the entity and append an error message to the method output.
            //•	Dates will always be in the format: "yyyy-MM-dd HH:mm:ss"
            //•	CultureInfo.InvariantCulture.
            //•	Projection datetime should be in the format "MM/dd/yyyy"

            #endregion
            var xmlSerializer = new XmlSerializer(typeof(ProjectionImportDto[]), new XmlRootAttribute("Projections"));
            var dtoCollection = (ProjectionImportDto[])xmlSerializer.Deserialize(new StringReader(xmlString));

            var projections = new List<Projection>();

            var sb = new StringBuilder();

            foreach (var projDto in dtoCollection)
            {
                var movieTarget = context.Movies.FirstOrDefault(m => m.Id == projDto.MovieId);
                var hallTarget = context.Halls.FirstOrDefault(h => h.Id == projDto.HallId);

                if (movieTarget == null || hallTarget == null)
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var projection = new Projection
                {
                    MovieId = projDto.MovieId,
                    HallId = projDto.HallId,
                    DateTime = DateTime.ParseExact(projDto.DateTime, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                };
                var title = context.Movies.FirstOrDefault(m => m.Id == projection.MovieId).Title;

                projections.Add(projection);
                sb.AppendLine(String.Format(SuccessfulImportProjection, title, projection.DateTime.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)));
            }
            
            context.Projections.AddRange(projections);
            context.SaveChanges();
            return sb.ToString().TrimEnd();
        }

        public static string ImportCustomerTickets(CinemaContext context, string xmlString)
        {
            #region Query
            // Using the file customers-tickets.xml, import the data from the file into the database.
            //Constraints
            //•	If there are any validation errors(such invalid names, age, balance, etc.), 
            //do not import any part of the entity and append an error message to the method output.
            #endregion

            var ser = new XmlSerializer(typeof(ImportCustomerDto[]), new XmlRootAttribute("Customers"));
            var collectionDTO = (ImportCustomerDto[])ser.Deserialize(new StringReader(xmlString));
            var customers = new List<Customer>();

            var sb = new StringBuilder();

            foreach (var dto in collectionDTO)
            {
                if (!IsValid(dto))
                {
                    sb.AppendLine(ErrorMessage);
                    continue;
                }

                var customer = new Customer
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Age = dto.Age,
                    Balance = dto.Balance
                };

                foreach (var ticket in dto.Tickets)
                {
                    if (!IsValid(ticket))
                    {
                        sb.AppendLine(ErrorMessage);
                        continue;
                    }
                    var currentTicket = new Ticket
                    {
                        ProjectionId = ticket.ProjectionId,
                        Price =  ticket.Price
                    };
                    customer.Tickets.Add(currentTicket);
                }

                customers.Add(customer);

                sb.AppendLine(String.Format(
                    SuccessfulImportCustomerTicket, customer.FirstName, customer.LastName, customer.Tickets.Count));
            }

            context.Customers.AddRange(customers);
            context.SaveChanges();

            return sb.ToString().TrimEnd();
        }

        private static bool IsValid(object dto)
        {
            var validationContext = new ValidationContext(dto);
            var validationResult = new List<ValidationResult>();

            return Validator.TryValidateObject(dto, validationContext, validationResult, true);
        }
    }
}