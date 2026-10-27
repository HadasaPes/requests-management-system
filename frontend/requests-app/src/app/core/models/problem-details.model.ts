/** RFC 7807 error body returned by the API for every 4xx/5xx. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  /** Validation errors (400): field name -> messages. */
  errors?: Record<string, string[]>;
  traceId?: string;
}
