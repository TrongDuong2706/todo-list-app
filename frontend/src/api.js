export async function request(path = '', options = {}) {
  const response = await fetch(`/api/todos${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers },
    signal: AbortSignal.timeout(15000),
  })
  if (!response.ok) {
    const body = await response.json().catch(() => null)
    const message = body?.errors ? Object.values(body.errors).flat().join(' ') : null
    throw new Error(message || (response.status === 404
      ? 'Công việc không còn tồn tại. Hãy tải lại danh sách.'
      : 'Không thể xử lý yêu cầu. Hãy kiểm tra backend và kết nối PostgreSQL.'))
  }
  return response.status === 204 ? null : response.json()
}
