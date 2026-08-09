import nodemailer from 'nodemailer'

let input = ''
for await (const chunk of process.stdin) input += chunk

try {
  const { username, appPassword, fromName, email, fullName, code } = JSON.parse(input)
  const escapeHtml = (value) => String(value).replace(/[&<>"']/g, character => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  })[character])

  const transporter = nodemailer.createTransport({
    host: 'smtp.gmail.com',
    port: 465,
    secure: true,
    auth: { user: username, pass: appPassword.replaceAll(' ', '') }
  })

  await transporter.sendMail({
    from: { name: fromName || 'School Requests', address: username },
    to: email,
    subject: `${code} is your School Requests verification code`,
    html: `<div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px"><h2 style="color:#0b2442">Verify your email</h2><p>Hello ${escapeHtml(fullName)},</p><p>Use this code to finish creating your student account:</p><div style="font-size:32px;font-weight:700;letter-spacing:8px;color:#1769e0;padding:18px 0">${code}</div><p>This code expires in 10 minutes. If you did not request it, you can ignore this email.</p></div>`
  })
  process.stdout.write(JSON.stringify({ sent: true }))
} catch (error) {
  process.stderr.write(error?.message || 'Email could not be sent.')
  process.exitCode = 1
}
