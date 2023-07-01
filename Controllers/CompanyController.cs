using DapperWebAPI.Contracts;
using DapperWebAPI.Dto;
using Microsoft.AspNetCore.Mvc;

namespace DapperWebAPI.Controllers
{
    [Route("api/company")]
    [ApiController]
    public class CompanyController : Controller
    {
        private readonly ICompanyRepository _companyRepository;

        public CompanyController(ICompanyRepository companyRepository)
        {
            _companyRepository = companyRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            var companies = await _companyRepository.GetCompanies();

            return Ok(companies);
        }

        [HttpGet("{id}", Name = "CompanyById")]
        public async Task<IActionResult> GetCompany(int id)
        {
            var company = await _companyRepository.GetCompany(id);

            if (company is null)
                return NotFound();

            return Ok(company);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] CompanyDto company)
        {
            var createdCompany = await _companyRepository.CreateCompany(company);

            return CreatedAtRoute("CompanyById", new { Id = createdCompany.Id }, createdCompany);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanyDto company)
        {
            var dbCompany = await _companyRepository.GetCompany(id);
            if (dbCompany is null)
                return NotFound();

            await _companyRepository.UpdateCompany(id, company);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id, [FromBody] CompanyDto company)
        {
            var dbCompany = await _companyRepository.GetCompany(id);
            if (dbCompany is null)
                return NotFound();

            await _companyRepository.DeleteCompany(id);

            return NoContent();
        }

        [HttpGet("ByEmployeeId/{id}")]
        public async Task<IActionResult> GetCompanyForEmployee(int id)
        {
            var company = await _companyRepository.GetCompanyByEmployeeId(id);
            if (company is null)
                return NotFound();

            return Ok(company);
        }

        [HttpGet("{id}/MultipleResult")]
        public async Task<IActionResult> GetMultipleResults(int id)
        {
            var company = await _companyRepository.GetMultipleResults(id);
            if (company is null)
                return NotFound();

            return Ok(company);
        }

        [HttpGet("MultipleMapping")]
        public async Task<IActionResult> GetMultipleMapping()
        {
            var companies = await _companyRepository.MultipleMapping();

            return Ok(companies);
        }

        [HttpPost("Multiple")]
        public async Task<IActionResult> CreateMultipleCompanies(List<CompanyDto> companies)
        {
            await _companyRepository.CreateMultipleCompanies(companies);

            return Ok(companies);
        }
    }
}