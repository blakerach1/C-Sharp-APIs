using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;

namespace DotNetApi.Helpers
{
    public class ReuseableSql
    {
        private readonly DataContextDapper _dapper;
        public ReuseableSql(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        public bool UpsertUser(UserComplete user)
        // end point is looking for a model to be passed in with the payload
        {
            string sql = @"EXEC TutorialAppSchema.spUser_Upsert
                @FirstName = FirstNameParam, 
                @LastName = @LastNameParam, 
                @Email = @EmailParam, 
                @Gender = @GenderParam, 
                @Active = @ActiveParam, 
                @JobTitle = @JobTitleParam, 
                @Department = @DepartmentParam, 
                @Salary = @SalaryParam, 
                @UserId= @UserIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@FirstNameParam", user.FirstName, DbType.String);
            sqlParameters.Add("@LastNameParam", user.LastName, DbType.String);
            sqlParameters.Add("@EmailParam", user.Email, DbType.String);
            sqlParameters.Add("@GenderParam", user.Gender, DbType.String);
            sqlParameters.Add("@ActiveParam", user.Active, DbType.Boolean);
            sqlParameters.Add("@JobTitleParam", user.JobTitle, DbType.String);
            sqlParameters.Add("@DepartmentParam", user.Department, DbType.String);
            sqlParameters.Add("@SalaryParam", user.Salary, DbType.Decimal);
            sqlParameters.Add("@UserIdParam", user.UserId, DbType.Int32);

            return _dapper.ExecuteSqlWithParameters(sql, sqlParameters);
        }
    }
}