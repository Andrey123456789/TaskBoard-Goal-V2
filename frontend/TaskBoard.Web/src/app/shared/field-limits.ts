/**
 * Approved maximum text lengths (CUSTOM_SETTINGS.md). The API enforces them; the UI mirrors
 * them for immediate feedback.
 */
export const FIELD_LIMITS = {
  userName: 200,
  userEmail: 320,
  projectName: 200,
  projectDescription: 4000,
  taskTitle: 200,
  taskDescription: 4000,
} as const;
