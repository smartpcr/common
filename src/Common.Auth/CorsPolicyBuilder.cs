// -----------------------------------------------------------------------
// <copyright file="CorsPolicyBuilder.cs" company="Microsoft Corp.">
//     Copyright (c) Microsoft Corp. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Common.Auth;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class CorsPolicyBuilder
{
    public const string RestApiPolicyName = "REST";
    public const string GraphQLPolicyName = "GraphQL";
    public const string GrpcPolicyName = "GrpcPolicy";
    public const string UploadMetaDataPolicyName = "MetaData";

    public static void AddCorsPolicies(this IServiceCollection services)
    {
        services
            .AddCors(options =>
            {
                options.AddPolicy(RestApiPolicyName, builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());

                options.AddPolicy(GraphQLPolicyName, builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());

                options.AddPolicy(GrpcPolicyName, builder =>
                {
                    builder.AllowAnyOrigin();
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();

                    // builder.WithOrigins("localhost:3000", "YourCustomDomain");
                    // builder.WithMethods("POST, OPTIONS");
                    // builder.AllowAnyHeader();
                    // builder.WithExposedHeaders("Grpc-Status", "Grpc-Message");
                });

                options.AddPolicy(UploadMetaDataPolicyName, builder =>
                    builder.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .WithMethods("POST")
                        .WithExposedHeaders("Content-Disposition"));
            });
    }

    public static void UseCorsPolicies(this IApplicationBuilder app)
    {
        app.UseCors(RestApiPolicyName);
        app.UseCors(GraphQLPolicyName);
        app.UseCors(GrpcPolicyName);
        app.UseCors(UploadMetaDataPolicyName);
    }
}