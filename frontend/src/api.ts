export async function ask(question: string) {
  const res = await fetch(`${import.meta.env.VITE_API_BASE}/api/chat`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ question }),
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}

export async function upload(file: File) {
  const form = new FormData()
  form.append('file', file)
  const res = await fetch(`${import.meta.env.VITE_API_BASE}/api/ingest`, {
    method: 'POST',
    body: form,
  })
  if (!res.ok) throw new Error(await res.text())
  return res.json()
}
