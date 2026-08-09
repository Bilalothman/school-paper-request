import { spawn } from 'node:child_process'

const npmCommand = process.platform === 'win32' ? 'npm.cmd' : 'npm'
const children = []
let stopping = false

function start(name, command, args, useShell = false) {
  const child = spawn(command, args, { cwd: process.cwd(), stdio: 'inherit', shell: useShell })
  children.push(child)
  child.on('error', (error) => {
    console.error(`${name} could not start: ${error.message}`)
    stopAll(1)
  })
  child.on('exit', (code) => {
    if (!stopping) {
      console.error(`${name} stopped with exit code ${code ?? 1}.`)
      stopAll(code ?? 1)
    }
  })
}

function stopAll(exitCode = 0) {
  if (stopping) return
  stopping = true
  for (const child of children) {
    if (!child.killed) child.kill()
  }
  setTimeout(() => process.exit(exitCode), 300)
}

console.log('Starting the ASP.NET Core API and React frontend...')
console.log('XAMPP MySQL and Camunda 7 must already be running.\n')

start('Backend', 'dotnet', ['run', '--project', 'Backend/Backend.csproj'])
start('Frontend', npmCommand, ['--prefix', 'frontend', 'run', 'dev'], process.platform === 'win32')

process.on('SIGINT', () => stopAll(0))
process.on('SIGTERM', () => stopAll(0))
