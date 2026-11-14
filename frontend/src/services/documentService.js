import { apiRequest, API_BASE_URL } from './apiClient'

export const getDocuments = () => apiRequest('/api/documents')
export const getDocument = (id) => apiRequest(`/api/documents/${id}`)
export const getDocumentStatus = (id) => apiRequest(`/api/documents/${id}/status`)
export const deleteDocument = (id) => apiRequest(`/api/documents/${id}`, { method: 'DELETE' })

export const getDocumentFileUrl = (id) => `${API_BASE_URL}/api/documents/${id}/file`

export async function uploadDocument(file) {
  const form = new FormData()
  form.append('file', file)
  return apiRequest('/api/documents', { method: 'POST', body: form })
}
