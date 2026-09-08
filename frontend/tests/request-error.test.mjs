import test from "node:test"
import assert from "node:assert/strict"
import { requestErrorMessage } from "../lib/request-error.ts"

test("model-validation messages survive when the API returns ProblemDetails", () => {
  const reason = { isAxiosError: true, response: { data: { errors: { Goal: ["The goal needs at least 10 characters."], Fields: ["Unknown field."] } } } }
  assert.equal(requestErrorMessage(reason), "The goal needs at least 10 characters. Unknown field.")
})
test("explicit server messages and network fallbacks remain readable", () => {
  assert.equal(requestErrorMessage({ isAxiosError: true, response: { data: { message: "Reload and retry." } } }), "Reload and retry.")
  assert.equal(requestErrorMessage(new Error("internal stack"), "Please retry."), "Please retry.")
})
