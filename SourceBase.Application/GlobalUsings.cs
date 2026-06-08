global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Routing;
global using SourceBase.Domain.Entities;

// Alias domain exceptions to avoid ambiguity with FluentValidation.ValidationException
global using ApiInternalException = SourceBase.Domain.ApiInternalException;
global using BadRequestException = SourceBase.Domain.BadRequestException;
global using ForbiddenException = SourceBase.Domain.ForbiddenException;
global using NotFoundException = SourceBase.Domain.NotFoundException;
global using UnAuthorizedException = SourceBase.Domain.UnAuthorizedException;
global using ValidationException = SourceBase.Domain.ValidationException;
