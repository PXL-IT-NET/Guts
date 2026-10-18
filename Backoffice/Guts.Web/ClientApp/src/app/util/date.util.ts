/**
 * Date utility functions replacing Moment.js with native Date and Intl APIs.
 */

export function formatDateTime(
  value: Date | string | number | null | undefined,
): string {
  if (!value) return "";
  const d =
    typeof value === "string" && !value.endsWith("Z") && !value.includes("+")
      ? new Date(value + "Z")
      : new Date(value);
  if (isNaN(d.getTime())) return "";
  const day = String(d.getDate()).padStart(2, "0");
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const year = d.getFullYear();
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${day}/${month}/${year} ${hours}:${minutes}`;
}

export function formatDateTimeLocal(
  value: Date | string | null | undefined,
): string {
  if (!value) return "";
  const d = typeof value === "string" ? new Date(value) : value;
  if (isNaN(d.getTime())) return "";
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  const hours = String(d.getHours()).padStart(2, "0");
  const minutes = String(d.getMinutes()).padStart(2, "0");
  return `${year}-${month}-${day}T${hours}:${minutes}`;
}

export function toIsoDateString(
  value: Date | string | null | undefined,
): string {
  if (!value) return "";
  if (value instanceof Date) return value.toISOString();
  return value;
}
