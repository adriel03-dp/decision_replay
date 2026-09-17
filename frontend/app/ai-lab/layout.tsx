import { ProtectedRoute } from "@/components/protected-route"
export default function AiLabLayout({ children }: { children: React.ReactNode }) { return <ProtectedRoute>{children}</ProtectedRoute> }
