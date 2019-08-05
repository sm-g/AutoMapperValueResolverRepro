using AutoMapper;
using FluentAssertions;

namespace AutoMapperRepro
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // arrange
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MyProfile>();
            });
            var executionPlan = config.BuildExecutionPlan(typeof(Source), typeof(Response));
            var mapper = config.CreateMapper();
            var source = new Source()
            {
                Resource = new Resource()
                {
                    Id = "ResourceId",
                    ConnectionId = "ConnectionId"
                }
            };

            // act
            var result = mapper.Map<Response>(source);

            // assert
            result.Resource.Should().BeEquivalentTo(new Response.ResourcePart
            {
                Id = source.Resource.Id,
                Connection = new Response.ConnectionPart
                {
                    Id = "ConnectionId",
                    Application = new Response.ApplicationPart
                    {
                        Id = "ApplicationId",
                    }
                }
            });
        }
    }

    public class MyProfile : Profile
    {
        public MyProfile()
        {
            CreateMap<Source, Response>();
            CreateMap<IResource, Response.ResourcePart>()
                .ForMember(x => x.Connection, o => o.MapFrom<ConnectionValueResolver<Response.ConnectionPart>>());
            CreateMap<IConnection, Response.ConnectionPart>()
                .ForMember(x => x.Application, o => o.MapFrom<ApplicationValueResolver<Response.ApplicationPart>>());
            CreateMap<IApplication, Response.ApplicationPart>();
        }
    }

    public class Source
    {
        public IResource Resource { get; set; }
    }

    public class Response
    {
        public ResourcePart Resource { get; set; }

        public class ResourcePart : IResource
        {
            public string Id { get; set; }
            public ConnectionPart Connection { get; set; }
            string IResource.ConnectionId { get; set; }
        }

        public class ConnectionPart : IConnection
        {
            public string Id { get; set; }
            public ApplicationPart Application { get; set; }
            string IConnection.ApplicationId { get; set; }
        }

        public class ApplicationPart : IApplication
        {
            public string Id { get; set; }
        }
    }

    public class ApplicationValueResolver<T> : IValueResolver<IConnection, object, T>
        where T : class
    {
        public T Resolve(IConnection source, object destination, T destMember, ResolutionContext context)
        {
            // this code runs twice
            // 1) source is Connection, created in ConnectionValueResolver
            // 2) source is ConnectionPart (with null ApplicationId), created in ConnectionValueResolver by mapper

            if (source.ApplicationId == null)
                return null;
            var application = new Application
            {
                Id = source.ApplicationId
            };
            var result = context.Mapper.Map<T>(application);
            return result;
        }
    }

    public class ConnectionValueResolver<T> :
        IValueResolver<IResource, object, T>
    {
        public T Resolve(IResource source, object destination, T destMember, ResolutionContext context)
        {
            var connection = new Connection
            {
                Id = source.ConnectionId,
                ApplicationId = "ApplicationId"
            };
            var result = context.Mapper.Map<T>(connection);
            return result;
        }
    }

    public interface IResource
    {
        string Id { get; set; }
        string ConnectionId { get; set; }
    }

    public class Resource : IResource
    {
        public string Id { get; set; }
        public string ConnectionId { get; set; }
    }

    public interface IConnection
    {
        string Id { get; set; }
        string ApplicationId { get; set; }
    }

    public class Connection : IConnection
    {
        public string Id { get; set; }
        public string ApplicationId { get; set; }
    }

    public interface IApplication
    {
        string Id { get; set; }
    }

    public class Application : IApplication
    {
        public string Id { get; set; }
    }
}