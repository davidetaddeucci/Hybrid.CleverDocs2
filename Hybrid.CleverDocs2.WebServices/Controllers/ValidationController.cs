using Microsoft.AspNetCore.Mvc;
using Hybrid.CleverDocs2.WebServices.Services.R2R.Clients;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Validation;
using Hybrid.CleverDocs2.WebServices.Services.R2R.DTOs.Validation;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Hybrid.CleverDocs2.WebServices.Controllers
{
    [ApiController]
    [Route("api/validations")]
    public class ValidationController : ControllerBase
    {
        private readonly IValidationClient _client;
        public ValidationController(IValidationClient client) => _client = client;

        [HttpPost("validate-data")]
        public async Task<IActionResult> ValidateData(ValidationRequest request) => Ok(await _client.ValidateDataAsync(request));

        [HttpPost("validate-schema")]
        public async Task<IActionResult> ValidateSchema(ValidationRequest request) => Ok(await _client.ValidateSchemaAsync(request));

        [HttpGet("results/{validationId}")]
        public async Task<IActionResult> GetValidationResult(string validationId) => Ok(await _client.GetValidationResultAsync(validationId));

        [HttpPost("validate-content")]
        public async Task<IActionResult> ValidateContent(ContentValidationRequest request) => Ok(await _client.ValidateContentAsync(request));

        [HttpPost("validate-compliance")]
        public async Task<IActionResult> ValidateCompliance(ComplianceValidationRequest request) => Ok(await _client.ValidateComplianceAsync(request));

        [HttpPost("validate-business-rules")]
        public async Task<IActionResult> ValidateBusinessRules(BusinessRuleValidationRequest request) => Ok(await _client.ValidateBusinessRulesAsync(request));
    }
}
