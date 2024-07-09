using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.UserSecrets;


namespace DotnetAPI.Controllers
{
    // [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PostController : ControllerBase
    {
        DataContextDapper _dapper;
        public PostController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
        }

        [HttpGet("Posts/{postId}/{userId}/{searchParam}")]
        public IEnumerable<Post> GetPosts(int postId = 0, int userId = 0, string searchParam = "None")
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Get";
            string stringParameters = "";

            DynamicParameters sqlParameters = new DynamicParameters();

            if (postId != 0)
            {
                stringParameters += ", @PostId=@PostIdParam";
                sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            }
            if (userId != 0)
            {
                stringParameters += ", @UserId=@UserIdParam";
                sqlParameters.Add("@UserIdParam", userId, DbType.Int32);
            }
            if (searchParam.ToLower() != "none")
            {
                stringParameters += ", @SearchValue=@SearchValueParam";
                sqlParameters.Add("@SearchValueParam", searchParam, DbType.String);
            }

            if (stringParameters.Length > 0)
            {
                sql += stringParameters[1..];
            }

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }


        [HttpGet("MyPosts")]
        public IEnumerable<Post> GetMyPosts()
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Get 
            @UserId = @UserIdParam";

            /* if userId existing in token, then it'll return the value - user 
            from ControllerBase class, rather than our user model. From "this" controller.
            Pull "claims" out of the token. */

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("UserIdParam", User.FindFirst("userId")?.Value, DbType.Int32);

            return _dapper.LoadDataWithParameters<Post>(sql, sqlParameters);
        }


        [HttpPut("UpsertPost")]
        public IActionResult UpsertPost(Post postToUpsert)
        {
            string sql = @"EXEC TutorialAppSchema.spPosts_Upsert
                @UserId =@UserIdParam,
                @PostTitle =@PostTitleParam,
                @PostContent =@PostContentParam";

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@UserIdParam", User.FindFirst("userId")?.Value, DbType.Int32);
            sqlParameters.Add("@PostTitleParam", postToUpsert.PostTitle, DbType.String);
            sqlParameters.Add("@PostContentParam", postToUpsert.PostContent, DbType.String);

            if (postToUpsert.PostId > 0)
            {
                sql += ", @PostId = @PostIdParam";
                sqlParameters.Add("@PostIdParam", postToUpsert.PostId, DbType.Int32);
            }

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to upsert post!");
        }


        [HttpDelete("DeletePost/{postId}")]
        public IActionResult DeletePost(int postId)
        {
            string sql = @"EXEC TutorialAppSchema.spPost_Delete
            @PostId =@PostIdParam,
            @UserId = @UserIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@PostIdParam", postId, DbType.Int32);
            sqlParameters.Add("@UserIdParam", User.FindFirst("userId")?.Value, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameters(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to delete post!");
        }

    }
}
