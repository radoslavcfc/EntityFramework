namespace Cinema.Data.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    public class Customer
    {
        public Customer()
        {
            this.Tickets = new HashSet<Ticket>();
        }
        public int Id { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(20, MinimumLength = 3)]
        public string LastName { get; set; }

        [Required]
        [Range(12, 110)]
        public int Age { get; set; }

        [Required]
        [Range(0.01, 9999999999999999.99)]
        public decimal Balance { get; set; }

        public ICollection<Ticket> Tickets { get; set; }

    }
}
