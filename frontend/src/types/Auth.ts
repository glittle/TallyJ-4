/**
 * Payload from the Telegram login widget. JSON numbers, not bigint.
 * `first_name` is always present on the widget callback and is required by the API.
 */
export interface TelegramWidgetUser {
  id: number;
  first_name: string;
  last_name?: string;
  username?: string;
  photo_url?: string;
  auth_date: number;
  hash: string;
}

/**
 * Teller and voter Telegram login body.
 * `id` and `authDate` stay numbers: hey-api types int64 as bigint, but its body
 * serializer turns bigint into a JSON string and the API expects a number.
 */
export interface TelegramLoginRequest {
  id: number;
  firstName: string;
  lastName?: string;
  username?: string;
  photoUrl?: string;
  authDate: number;
  hash: string;
}
