using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Aptabase.Data;
using System;

namespace Aptabase.Features.Authentication
{
    public class AccessControlFilter : ActionFilterAttribute
    {
        private readonly IDbContext _db;

        public AccessControlFilter(IDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            var appId = context.HttpContext.Request.Query["AppId"].ToString();

            var id = _db.Connection.ExecuteScalarAsync<string>(
                @"SELECT a.id
                  FROM apps a
                  LEFT JOIN app_shares s
                  ON s.app_id = a.id
                  WHERE a.id = @appId
                  AND (a.owner_id = @userId OR s.email = @userEmail)
                  LIMIT 1",
                  new { appId, userId = user.Id, userEmail = user.Email }
            ).Result;

            if (id != appId)
            {
                context.Result = new UnauthorizedResult();
            }

            base.OnActionExecuting(context);
        }
    }
}