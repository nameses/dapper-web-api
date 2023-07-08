using Dapper;
using DapperWebAPI.Context;
using DapperWebAPI.Contracts;
using DapperWebAPI.Dto;
using DapperWebAPI.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Data;
using System.Text.Json;

namespace DapperWebAPI.Repository
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly string allCompaniesCacheName = "all_companies_cache";
        private readonly string companyCacheName = "company_cache_";
        private readonly string companyEmployeesCacheName = "company_with_employees_cache_";

        private readonly DapperContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<CompanyRepository> _logger;

        public CompanyRepository(DapperContext сontext, IDistributedCache cache, ILogger<CompanyRepository> logger)
        {
            _context = сontext;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<Company>> GetCompanies()
        {
            IEnumerable<Company> companies;
            var companiesCache = await _cache.GetStringAsync(allCompaniesCacheName);
            if (companiesCache != null)
            {
                _logger.LogInformation($"Successfully got all companies from cache.");
                return JsonSerializer.Deserialize<IEnumerable<Company>>(companiesCache).ToList();
            }

            var query = "select * from Company";
            using (var connection = _context.CreateConnection())
            {
                companies = await connection.QueryAsync<Company>(query);
                if (companies is null)
                    _logger.LogError($"Failed to get all companies from DB");
                else
                {
                    _logger.LogInformation($"Successfully got all companies from DB");
                    companiesCache = JsonSerializer.Serialize(companies);
                    await _cache.SetStringAsync(allCompaniesCacheName, companiesCache, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    _logger.LogInformation($"Added list of companies to cache.");
                }
                return companies.ToList();
            }
        }

        public async Task<Company> GetCompany(int id)
        {
            Company company;
            var companyCache = await _cache.GetStringAsync(companyCacheName + id);
            if (companyCache != null)
            {
                _logger.LogInformation($"Successfully got company from cache.");
                return JsonSerializer.Deserialize<Company>(companyCache);
            }

            var query = "select * from Company where Id=@Id";
            using (var connection = _context.CreateConnection())
            {
                company = await connection.QuerySingleOrDefaultAsync<Company>(query, new { id });
                if (company is null)
                    _logger.LogError($"Failed to load company(ID {id}) from DB");
                else
                {
                    _logger.LogInformation($"Successfully get company(ID {id}) from DB");
                    companyCache = JsonSerializer.Serialize(company);
                    await _cache.SetStringAsync(companyCacheName + id, companyCache, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    _logger.LogInformation($"Added company(ID {id}) to cache.");
                }
                return company;
            }
        }

        public async Task<Company> CreateCompany(CompanyDto company)
        {
            var query = "INSERT INTO Company (Name, Address, Country) VALUES (@Name, @Address, @Country);" +
                "SELECT CAST(SCOPE_IDENTITY() AS int)";

            var parameters = new DynamicParameters();
            parameters.Add("Name", company.Name, System.Data.DbType.String);
            parameters.Add("Address", company.Address, System.Data.DbType.String);
            parameters.Add("Country", company.Country, System.Data.DbType.String);

            using (var connection = _context.CreateConnection())
            {
                var id = await connection.QuerySingleAsync<int>(query, parameters);

                return new Company()
                {
                    Id = id,
                    Name = company.Name,
                    Address = company.Address,
                    Country = company.Country
                };
            }
        }

        public async Task UpdateCompany(int id, CompanyDto company)
        {
            var query = "UPDATE Company SET Name=@Name,Address=@Address,Country=@Country WHERE Id=@Id";

            var parameters = new DynamicParameters();
            parameters.Add("Id", id, System.Data.DbType.String);
            parameters.Add("Name", company.Name, System.Data.DbType.String);
            parameters.Add("Address", company.Address, System.Data.DbType.String);
            parameters.Add("Country", company.Country, System.Data.DbType.String);

            using (var connection = _context.CreateConnection())
            {
                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task DeleteCompany(int id)
        {
            var query = "DELETE FROM Company WHERE Id = @Id";

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.ExecuteAsync(query, new { id });
                if (result < 1) _logger.LogError($"Failed to delete company(ID {id}) from DB");
            }
        }

        public async Task<Company> GetCompanyByEmployeeId(int id)
        {
            Company company;
            var companyCache = await _cache.GetStringAsync(companyEmployeesCacheName + id);
            if (companyCache != null)
            {
                _logger.LogInformation($"Successfully got company from cache.");
                return JsonSerializer.Deserialize<Company>(companyCache);
            }

            var procedureName = "ShowCompanyByEmployeeId";
            var parameters = new DynamicParameters();
            parameters.Add("Id", id, System.Data.DbType.Int32, System.Data.ParameterDirection.Input);

            using (var connection = _context.CreateConnection())
            {
                company = await connection.QueryFirstOrDefaultAsync<Company>
                    (procedureName, parameters, commandType: System.Data.CommandType.StoredProcedure);

                if (company is null)
                    _logger.LogError($"Failed to get company by employee ID {id} from DB");
                else
                {
                    _logger.LogInformation($"Successfully get company by employee ID {id} from DB");

                    companyCache = JsonSerializer.Serialize(company);
                    await _cache.SetStringAsync(companyEmployeesCacheName + id, companyCache, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                    _logger.LogInformation($"Added company(ID {id}) to cache.");
                }

                return company;
            }
        }

        public async Task<Company> GetMultipleResults(int id)
        {
            var query = "SELECT * FROM Company WHERE Id=@Id;" +
                "SELECT * FROM Employee WHERE CompanyId=@Id";

            using (var connection = _context.CreateConnection())
            using (var multi = await connection.QueryMultipleAsync(query, new { id }))
            {
                var company = await multi.ReadSingleOrDefaultAsync<Company>();
                if (company is not null)
                    company.Employees = (await multi.ReadAsync<Employee>()).ToList();

                if (company is null)
                    _logger.LogError($"Failed to get company(ID {id}) from DB");
                else
                    _logger.LogInformation($"Successfully get company(ID {id}) from DB");

                return company;
            }
        }

        public async Task<List<Company>> MultipleMapping()
        {
            var query = "SELECT * FROM Company c JOIN Employee e ON c.id=e.CompanyId";

            using (var connection = _context.CreateConnection())
            {
                var companyDict = new Dictionary<int, Company>();

                var companies = await connection.QueryAsync<Company, Employee, Company>(
                    query, (company, employee) =>
                    {
                        if (!companyDict.TryGetValue(company.Id, out var currentCompany))
                        {
                            currentCompany = company;
                            companyDict.Add(currentCompany.Id, currentCompany);
                        }

                        currentCompany.Employees.Add(employee);

                        return currentCompany;
                    }
                );
                return companies.Distinct().ToList();
            }
        }

        public async Task CreateMultipleCompanies(List<CompanyDto> companies)
        {
            var query = "INSERT INTO Company (Name, Address, Country) VALUES (@Name, @Address, @Country)";
            using (var connection = _context.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var company in companies)
                    {
                        var parameters = new DynamicParameters();
                        parameters.Add("Name", company.Name, DbType.String);
                        parameters.Add("Address", company.Address, DbType.String);
                        parameters.Add("Country", company.Country, DbType.String);
                        await connection.ExecuteAsync(query, parameters, transaction: transaction);
                    }
                    transaction.Commit();
                }
            }
        }
    }
}