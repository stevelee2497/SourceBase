using AutoMapper;
using Core.DbContexts;
using Core.Entities;
using Core.Extensions;
using Core.Helpers;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System.Text.Json;

namespace Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<UserEntity> _userManager;
        private readonly ISessionUserHelper _sessionUserHelper;
        private readonly IUserClaimsPrincipalFactory<UserEntity> _claimsFactory;

        public AuthService(UserManager<UserEntity> userManager, IUserClaimsPrincipalFactory<UserEntity> claimsFactory, ISessionUserHelper sessionUserHelper, IMapper mapper, ApplicationDbContext context)
        {
            _mapper = mapper;
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _sessionUserHelper = sessionUserHelper;
            _context = context;
        }

        public async Task Register(AuthRequestDto registration)
        {
            var user = new UserEntity { Email = registration.Email, UserName = registration.Email };
            var result = await _userManager.CreateAsync(user, registration.Password);

            if (!result.Succeeded)
            {
                throw new Exception(JsonSerializer.Serialize(result.Errors));
            }
        }

        public async Task Login(AuthRequestDto login)
        {
            var user = await _userManager.FindByNameAsync(login.Email);
            if (user == null)
            {
                throw new Exception(SignInResult.Failed.ToString());
            }

            var validPassword = await _userManager.CheckPasswordAsync(user, login.Password);
            if (validPassword == false)
            {
                throw new Exception(SignInResult.Failed.ToString());
            }

            var userPrincipal = await _claimsFactory.CreateAsync(user);
            foreach (var claim in new Claim[] { new Claim("amr", "pwd") })
            {
                userPrincipal.Identities.First().AddClaim(claim);
            }

            await _sessionUserHelper.SignInAsync(userPrincipal);
        }

        public async Task<UserInfoDto> GetUserInfo()
        {
            var userEntity = await _context.Users.FindAsync(Guid.Parse(_sessionUserHelper.UserId));

            return userEntity.MapTo<UserInfoDto>(_mapper);
        }

        public async Task<UserInfoDto> UpdateUserInfo(UserInfoDto userInfoDto)
        {
            var userEntity = await _context.Users.FindAsync(Guid.Parse(_sessionUserHelper.UserId));

            userEntity.FirstName = userInfoDto.FirstName;
            userEntity.LastName = userInfoDto.LastName;

            await _context.SaveChangesAsync();

            return userEntity.MapTo<UserInfoDto>(_mapper);
        }
    }
}
