import { redirect } from "next/navigation"

export default async function DecisionPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params
  redirect(`/decisions/${id}/analytics`)
}
