export default function RequestStatus({ status }) {
  return <span className={`status status-${status.toLowerCase()}`}>{status}</span>
}
