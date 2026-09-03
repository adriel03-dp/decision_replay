"use client"

import { useEffect, useState } from "react"
import axios from "axios"
import { requirementsApi } from "@/lib/requirements-api"
import { decisionApi } from "@/lib/api"
import { draftFromReview, requirementPayload } from "@/lib/requirement-draft"
import type { RequirementDomain, RequirementDraft, RequirementsReview } from "@/lib/requirements-types"
import { requestErrorMessage } from "@/lib/request-error"

export function useRequirementReview() {
  const [domains, setDomains] = useState<RequirementDomain[]>([])
  const [draft, setDraft] = useState<RequirementDraft | null>(null)
  const [review, setReview] = useState<RequirementsReview | null>(null)
  const [dirty, setDirty] = useState(false)
  const [acknowledged, setAcknowledged] = useState(false)
  const [busy, setBusy] = useState<"preview" | "validate" | "create" | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [schemaError, setSchemaError] = useState<string | null>(null)
  const [schemaAttempt, setSchemaAttempt] = useState(0)

  useEffect(() => {
    const cancellation = new AbortController()
    requirementsApi.domains(cancellation.signal).then(value => { setDomains(value); setSchemaError(null) })
      .catch(reason => { if (!axios.isCancel(reason)) setSchemaError("Field definitions could not be loaded. Retry before reviewing requirements.") })
    return () => cancellation.abort()
  }, [schemaAttempt])

  function fail(reason: unknown) {
    setError(requestErrorMessage(reason))
  }
  function edit(next: RequirementDraft) { setDraft(next); setDirty(true); setAcknowledged(false); setError(null) }
  function reset() { setDraft(null); setReview(null); setDirty(false); setAcknowledged(false); setError(null) }
  async function preview(input: string, fixedFields: string[]) {
    setBusy("preview"); setError(null)
    try { const result = await requirementsApi.preview(input, fixedFields); setDraft(draftFromReview(result, input)); setReview(result); setDirty(false); setAcknowledged(false) }
    catch (reason) { fail(reason) } finally { setBusy(null) }
  }
  const domain = domains.find(item => item.domain === draft?.domain)
  async function validate() {
    if (!draft || !domain) return
    setBusy("validate"); setError(null)
    try { const result = await requirementsApi.validate(requirementPayload(draft, domain)); setDraft(draftFromReview(result, draft.originalInput)); setReview(result); setDirty(false); setAcknowledged(false) }
    catch (reason) { fail(reason) } finally { setBusy(null) }
  }
  async function create() {
    if (!draft || !domain || dirty || !review?.canSubmit || !acknowledged || busy) return
    setBusy("create"); setError(null)
    try { return await decisionApi.analyzeStructured(requirementPayload(draft, domain)) }
    catch (reason) { fail(reason) } finally { setBusy(null) }
  }
  return { domains, domain, draft, review, dirty, acknowledged, setAcknowledged, busy, error, schemaError,
    retrySchema: () => setSchemaAttempt(attempt => attempt + 1), edit, reset, preview, validate, create }
}
