using DapperWebAPI.Dto;
using DapperWebAPI.Entities;

namespace DapperWebAPI.Contracts
{
    public interface ICompanyRepository
    {
        public Task<List<Company>> GetCompanies();

        public Task<Company> GetCompany(int id);

        public Task<Company> CreateCompany(CompanyDto company);

        public Task UpdateCompany(int id, CompanyDto company);

        public Task DeleteCompany(int id);

        public Task<Company> GetCompanyByEmployeeId(int id);

        public Task<Company> GetMultipleResults(int id);

        public Task<List<Company>> MultipleMapping();

        public Task CreateMultipleCompanies(List<CompanyDto> companies);
    }
}