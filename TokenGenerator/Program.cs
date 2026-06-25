
// TokenGenerator.cs — paste into a dotnet-script or a scratch console app
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

var key = "YourActualSecretKeyThatIsAtLeast32Characters!"; // must match appsettings.json Jwt:Key
var issuer = "FormsDataManagementAPI";
var audience = "FormsDataManagementAPI";

var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, "Sowmya"),
    new Claim(ClaimTypes.Name, "Chidambaram"),
    new Claim(ClaimTypes.Role, "Admin") // adjust as needed
};

var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

var token = new JwtSecurityToken(
    issuer: issuer,
    audience: audience,
    claims: claims,
    expires: DateTime.UtcNow.AddHours(1),
    signingCredentials: creds
);

Console.WriteLine(new JwtSecurityTokenHandler().WriteToken(token));

