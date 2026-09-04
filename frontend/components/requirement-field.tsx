"use client"

import type { RequirementField as FieldDefinition } from "@/lib/requirements-types"

export function RequirementField({ field, value, onChange, disabled, invalid }: {
  field: FieldDefinition; value: string; onChange: (value: string) => void; disabled: boolean; invalid: boolean
}) {
  const id = `requirement-${field.name}`
  const classes = "mt-2 min-h-11 w-full rounded-lg border border-white/20 bg-slate-950 px-3 py-2 text-sm text-slate-100 outline-none focus-visible:border-sky-300 focus-visible:ring-2 focus-visible:ring-sky-300/30 disabled:opacity-60"
  const common = { id, value, disabled, "aria-invalid": invalid, "aria-describedby": `${id}-help`, maxLength: 20000 }
  return <div className="min-w-0">
    <label htmlFor={id} className="text-sm font-medium capitalize text-slate-100">{field.name.replaceAll("_", " ")}{field.required && <span className="ml-1 text-slate-400">(required context)</span>}</label>
    {field.type === "List" ? <textarea {...common} rows={3} className={classes} onChange={event => onChange(event.target.value)} />
      : <input {...common} type={field.type === "Date" ? "date" : "text"} inputMode={["Number", "Currency", "Integer"].includes(field.type) ? "decimal" : "text"} list={field.type === "Level" ? `${id}-levels` : undefined} className={classes} onChange={event => onChange(event.target.value)} />}
    {field.type === "Level" && <datalist id={`${id}-levels`}>{["unknown", "none", "low", "partial", "medium", "high", "ready"].map(level => <option key={level} value={level} />)}</datalist>}
    <p id={`${id}-help`} className={`mt-1 text-sm leading-5 ${invalid ? "text-red-300" : "text-slate-400"}`}>{invalid ? "This value could not be interpreted. Correct it or clear it to leave an explicit gap." : field.description}</p>
  </div>
}
