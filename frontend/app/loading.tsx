export default function Loading() {
  return <div role="status" aria-live="polite" className="mx-auto max-w-7xl px-5 py-12">
    <p className="text-sm text-muted-foreground">Opening workspace…</p>
    <div className="mt-5 h-8 w-52 animate-pulse rounded bg-muted" />
    <div className="mt-8 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {[0, 1, 2].map(item => <div key={item} className="h-40 animate-pulse rounded-xl bg-muted" />)}
    </div>
  </div>
}
