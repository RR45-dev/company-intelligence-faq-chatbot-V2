import React, { useState } from 'react'
import { ask, upload } from './api'

type Message = { sender: 'user' | 'bot', text: string }

export default function App() {
  const [messages, setMessages] = useState<Message[]>([])
  const [input, setInput] = useState('')
  const [busy, setBusy] = useState(false)
  const [fileBusy, setFileBusy] = useState(false)

  const onSend = async () => {
    const q = input.trim()
    if (!q) return
    setInput('')
    setMessages(m => [...m, { sender: 'user', text: q }])
    setBusy(true)
    try {
      const data = await ask(q)
      setMessages(m => [...m, { sender: 'bot', text: data.answer }])
    } catch (e: any) {
      setMessages(m => [...m, { sender: 'bot', text: 'Error: ' + e.message }])
    } finally {
      setBusy(false)
    }
  }

  const onUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (!f) return
    setFileBusy(true)
    try {
      const res = await upload(f)
      setMessages(m => [...m, { sender: 'bot', text: `Ingested ${res.chunksCreated} chunks from ${res.fileName}` }])
    } catch (e: any) {
      alert('Upload failed: ' + e.message)
    } finally {
      setFileBusy(false)
      e.target.value = ''
    }
  }

  return (
    <div style={{ maxWidth: 780, margin: '0 auto', padding: 24, fontFamily: 'Inter, system-ui, sans-serif' }}>
      <h1>Company Intelligence FAQ Chatbot</h1>
      <p>Upload a PDF/TXT doc, then ask questions. Answers are grounded in your ingested content.</p>

      <div style={{ margin: '16px 0' }}>
        <input type="file" accept=".pdf,.txt" onChange={onUpload} disabled={fileBusy} />
        {fileBusy && <span style={{ marginLeft: 8 }}>Uploading & embedding…</span>}
      </div>

      <div style={{ border: '1px solid #ddd', padding: 16, borderRadius: 8, minHeight: 240 }}>
        {messages.map((m, i) => (
          <div key={i} style={{ margin: '8px 0' }}>
            <b>{m.sender === 'user' ? 'You' : 'Bot'}</b>
            <div>{m.text}</div>
          </div>
        ))}
        {messages.length === 0 && <div style={{ color: '#666' }}>No messages yet.</div>}
      </div>

      <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
        <input
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={e => e.key === 'Enter' ? onSend() : null}
          placeholder="Ask something like “What’s our refund policy?”"
          style={{ flex: 1, padding: 12, borderRadius: 8, border: '1px solid #ccc' }}
          disabled={busy}
        />
        <button onClick={onSend} disabled={busy || !input.trim()} style={{ padding: '12px 16px', borderRadius: 8 }}>
          {busy ? 'Thinking…' : 'Send'}
        </button>
      </div>
    </div>
  )
}
