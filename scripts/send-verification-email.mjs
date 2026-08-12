import nodemailer from 'nodemailer'

let input = ''
for await (const chunk of process.stdin) input += chunk

try {
  const { username, appPassword, fromName, message } = JSON.parse(input)
  const { email, fullName, notificationType, code, isPasswordReset = false, requestId, serviceName, decision, adminComment } = message
  const escapeHtml = (value) => String(value).replace(/[&<>"']/g, character => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  })[character])

  const transporter = nodemailer.createTransport({
    host: 'smtp.gmail.com',
    port: 465,
    secure: true,
    auth: { user: username, pass: appPassword.replaceAll(' ', '') }
  })

  const isDecision = notificationType === 'requestDecision'
  const isResultImageReady = notificationType === 'resultImageReady'
  const approved = decision === 'Approved'
  const subject = isResultImageReady
    ? `Your ${serviceName} document is ready`
    : isDecision
      ? `Your ${serviceName} request was ${decision.toLowerCase()}`
      : `${code} is your School Requests ${isPasswordReset ? 'password reset' : 'verification'} code`
  const html = isResultImageReady
    ? `<div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px"><h2 style="color:#1769e0">Your document is ready</h2><p>Hello ${escapeHtml(fullName)},</p><p>The result image for your request #${escapeHtml(requestId)} for <strong>${escapeHtml(serviceName)}</strong> has been added.</p><p>Sign in to School Requests and open your request to view or download it.</p></div>`
    : isDecision
      ? `<div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px"><h2 style="color:${approved ? '#18794e' : '#b42318'}">Request ${escapeHtml(decision)}</h2><p>Hello ${escapeHtml(fullName)},</p><p>Your request #${escapeHtml(requestId)} for <strong>${escapeHtml(serviceName)}</strong> has been <strong>${escapeHtml(decision.toLowerCase())}</strong>.</p>${adminComment ? `<p><strong>Administrator comment:</strong><br>${escapeHtml(adminComment)}</p>` : ''}<p>You can sign in to School Requests to view the latest details.</p></div>`
      : `<div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px"><h2 style="color:#0b2442">${isPasswordReset ? 'Reset your password' : 'Verify your email'}</h2><p>Hello ${escapeHtml(fullName)},</p><p>${isPasswordReset ? 'Use this code to choose a new password:' : 'Use this code to finish creating your student account:'}</p><div style="font-size:32px;font-weight:700;letter-spacing:8px;color:#1769e0;padding:18px 0">${code}</div><p>This code expires in 10 minutes. If you did not request it, you can ignore this email.</p></div>`

  await transporter.sendMail({
    from: { name: fromName || 'School Requests', address: username },
    to: email,
    subject,
    html
  })
  process.stdout.write(JSON.stringify({ sent: true }))
} catch (error) {
  process.stderr.write(error?.message || 'Email could not be sent.')
  process.exitCode = 1
}
