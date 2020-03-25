namespace Cinema
{
    using AutoMapper;
    using Cinema.Data.Models;
    using Cinema.DataProcessor.ImportDto;
    public class CinemaProfile : Profile
    {
        public CinemaProfile()
        {
            CreateMap<MoviesImportDto, Movie>();
            CreateMap<HallImportDto, Hall>();
        }
    }
}
