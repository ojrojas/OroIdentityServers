# OAuth 2.1 Authorization Code Flow with PKCE Example

This example demonstrates the Authorization Code flow with PKCE (Proof Key for Code Exchange), which is mandatory in OAuth 2.1 for SPA and mobile applications.

## What is PKCE?

PKCE prevents authorization code interception attacks by adding a cryptographic challenge that the client must solve during the code-to-token exchange.

## How it works

1. **PKCE Generation**: The client generates a random `code_verifier` and calculates its SHA256 hash to create the `code_challenge`.

2. **Authorization Request**: The client includes `code_challenge` and `code_challenge_method=S256` in the authorization URL.

3. **Code Exchange**: The client sends the `code_verifier` along with the authorization code to obtain tokens.

4. **Verification**: The server verifies that SHA256(code_verifier) == stored code_challenge.

## Running the example

### Option 1: Using the web interface (Recommended)

1. **Start the server**:
   ```bash
   cd examples/OroIdentityServerExample
   dotnet run
   ```

2. **Generate PKCE parameters** (run the PKCE example):
   ```bash
   cd examples/OroIdentityServerPKCEExample
   dotnet run
   ```

3. **Copy the authorization URL** shown by the program and paste it in your browser

4. **Complete the login** in the web interface

5. **On the callback page**, click "Exchange Code for Tokens"

6. **When the PKCE form appears**, paste the `code_verifier` shown by the console program

7. **Done!** You'll see the obtained tokens

### Option 2: Complete console flow

If you prefer the fully automated flow:

1. Run the PKCE program
2. Copy the authorization URL and open it in the browser
3. Complete the login
4. Copy the `code` from the callback URL
5. Paste it in the console when prompted
6. The program will automatically exchange the code for tokens using PKCE

## Expected output

```
OAuth 2.1 Authorization Code Flow with PKCE Example
=================================================

1. Generating PKCE parameters...
Code Verifier: abc123...xyz789
Code Challenge: sha256_hash_base64url

2. Authorization URL (would open in browser):
http://localhost:5160/connect/authorize?response_type=code&client_id=web-client&redirect_uri=...&code_challenge=...&code_challenge_method=S256

3. Simulating user authentication...
   Assuming we received authorization code: 'auth_code_here'

4. Please open the authorization URL in your browser and complete the login.
   After authorization, copy the 'code' parameter from the callback URL.
   Enter the authorization code: [paste_code_here]

5. Exchanging authorization code for tokens with PKCE...
Token Response: {
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "id_token": "...",
  "refresh_token": "..."
}

6. Using access token to call protected API...
API Response: Hello alice! Your claims: name: Alice, email: alice@example.com

7. Refreshing access token...
Refresh Response: {
  "access_token": "...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "..."
}
```

## Enhanced security with PKCE

- **Without PKCE**: An attacker who intercepts the authorization code can exchange it for tokens
- **With PKCE**: The attacker also needs the secret `code_verifier` to complete the exchange

This makes the Authorization Code flow secure even in public applications (SPAs, mobile) where the client_secret cannot be kept secret.