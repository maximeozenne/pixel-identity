﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.MongoDb.Models;
using Pixel.Identity.Provider.Extensions;
using Pixel.Identity.Shared.Responses;
using Pixel.Identity.Shared.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Pixel.Identity.Provider.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IOpenIddictApplicationManager applicationManager;

        public ApplicationsController(IMapper mapper, IOpenIddictApplicationManager applicationManager)
        {
            this.mapper = mapper;
            this.applicationManager = applicationManager;
        }

        [HttpGet()]
        public async  Task<IEnumerable<ApplicationViewModel>> GetAll()
        {
            List<ApplicationViewModel> applicationDescriptors = new List<ApplicationViewModel>();
            await foreach(var app in this.applicationManager.ListAsync(null, null, CancellationToken.None))
            {
                var applicationDescriptor = mapper.Map<ApplicationViewModel>(app);
                applicationDescriptor.ClientSecret = string.Empty;               
                applicationDescriptors.Add(applicationDescriptor);
            }
            return applicationDescriptors;        
        }

        [HttpGet("clientId/{clientId}")]
        public async Task<ApplicationViewModel> Get(string clientId)
        {
            var app = await applicationManager.FindByClientIdAsync(clientId, CancellationToken.None);
            var applicationDescriptor = mapper.Map<ApplicationViewModel>(app);
            applicationDescriptor.ClientSecret = string.Empty;
            if (app is OpenIddictMongoDbApplication mongoApp)
            {
                applicationDescriptor.Id = mongoApp.Id.ToString();
            }          
            return applicationDescriptor;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] ApplicationViewModel applicationDescriptor)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var openIdApplicationDescriptor = mapper.Map<OpenIddictApplicationDescriptor>(applicationDescriptor);
                    await applicationManager.CreateAsync(openIdApplicationDescriptor, CancellationToken.None);
                    return Ok(new OkResponse(""));
                }
                return BadRequest(new BadRequestResponse(ModelState.GetValidationErrors()));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemResponse(ex.Message));
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] ApplicationViewModel applicationDescriptor)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(applicationDescriptor.Id))
                    {
                        return BadRequest(new BadRequestResponse(new[] { "Missing Id on model." }));
                    }
                    var existing = await applicationManager.FindByIdAsync(applicationDescriptor.Id);
                    if (existing != null)
                    {
                        var openIdApplicationDescriptor = mapper.Map<OpenIddictApplicationDescriptor>(applicationDescriptor);
                        await applicationManager.UpdateAsync(existing, openIdApplicationDescriptor, CancellationToken.None);
                        return Ok();
                    }
                    return NotFound(new NotFoundResponse($"Failed to find application with Id : {applicationDescriptor.Id}"));
                }
                return BadRequest(new BadRequestResponse(ModelState.GetValidationErrors()));
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemResponse(ex.Message));
            }
        }
    }
}
