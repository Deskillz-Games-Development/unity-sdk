// =============================================================================
// Deskillz SDK for Unity -- auth request bodies  [P1-3.7.0]
// Match backend auth.dto.ts: LoginDto { email, password }, RegisterDto
// { email, password, username }.
// =============================================================================
using System;

namespace Deskillz
{
    [Serializable]
    public sealed class LoginRequest
    {
        public string email;
        public string password;
        public LoginRequest(string email, string password) { this.email = email; this.password = password; }
    }

    [Serializable]
    public sealed class SignUpRequest
    {
        public string email;
        public string password;
        public string username;
        public SignUpRequest(string email, string password, string username)
        {
            this.email = email; this.password = password; this.username = username;
        }
    }
}
