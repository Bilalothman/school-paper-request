import { OAuth2Client } from 'google-auth-library'

let input = ''
for await (const chunk of process.stdin) input += chunk

try {
  const { credential, clientId } = JSON.parse(input)
  const client = new OAuth2Client()
  const ticket = await client.verifyIdToken({ idToken: credential, audience: clientId })
  const payload = ticket.getPayload()
  process.stdout.write(JSON.stringify({
    subject: payload.sub,
    email: payload.email,
    emailVerified: payload.email_verified === true,
    name: payload.name || ''
  }))
} catch (error) {
  process.stderr.write(error?.message || 'Invalid Google credential.')
  process.exitCode = 1
}
