import { ProtectedRoute } from '@/components/protected-route'

export default function DecisionsDashboardLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return <ProtectedRoute>{children}</ProtectedRoute>
}
