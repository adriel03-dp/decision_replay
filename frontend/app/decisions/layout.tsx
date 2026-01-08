import { ProtectedRoute } from '@/components/protected-route'

export default function DecisionsLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <ProtectedRoute>{children}</ProtectedRoute>
}
