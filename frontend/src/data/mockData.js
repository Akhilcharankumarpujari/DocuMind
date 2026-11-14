export const initialDocuments = [
  { id: 'doc-1', name: 'RAG_Survey_Paper.pdf', pages: 24, size: '2.4 MB', updated: 'Today', status: 'Ready' },
  { id: 'doc-2', name: 'Cloud_Computing_Notes.pdf', pages: 18, size: '1.8 MB', updated: 'Yesterday', status: 'Ready' },
  { id: 'doc-3', name: 'Project_Proposal.pdf', pages: 12, size: '940 KB', updated: 'Nov 10', status: 'Ready' },
]

export const starterMessages = [
  {
    id: 'welcome',
    role: 'assistant',
    content: "Hello! I've reviewed your document. You can ask me anything about its content, request a summary, or explore a specific section.",
    sources: [],
  },
]
