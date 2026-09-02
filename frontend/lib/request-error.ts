import axios from "axios"

export function requestErrorMessage(reason: unknown, fallback = "The request failed. Your requirements have been kept."): string {
  if (!axios.isAxiosError(reason)) return fallback
  const data = reason.response?.data
  if (typeof data?.message === "string" && data.message.trim()) return data.message
  if (data?.errors && typeof data.errors === "object") {
    const messages = Object.values(data.errors).flatMap(value => Array.isArray(value) ? value.filter(item => typeof item === "string") : [])
    if (messages.length) return messages.join(" ")
  }
  return fallback
}
