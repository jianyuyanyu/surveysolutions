using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WB.UI.Headquarters.Controllers.Api.PublicApi.Graphql
{
    public static class GraphQLIntegration
    {
        public static void AddGraphQL(this IServiceCollection services)
        {
            services.AddDataLoaderRegistry()
                .AddGraphQL(x => HeadquartersSchema);
        }

        public static ISchema HeadquartersSchema => SchemaBuilder.New()
            .AddAuthorizeDirectiveType()
            .AddType(new PaginationAmountType(200))
            .AddQueryType<HeadquartersQuery>().Create(); 

        public static IApplicationBuilder UseGraphQLApi(this IApplicationBuilder app)
        {
           return app.UseGraphQLHttpPost(new HttpPostMiddlewareOptions
                {
                    Path = "/graphql"
                })
                .UseGraphQLHttpGetSchema(new HttpGetSchemaMiddlewareOptions {Path = "/graphql/schema"});
        }
    }
}