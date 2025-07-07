#Техническое задание:
# статические данные и методы для работы, общие для всех endpoint.

#    match_type_time_limit: dict[str, int] - время на матч в секундах для каждого типа матча:
#         - 1v1
#         - 2v2
# по истечению времени матча, матч отменяется, если не былл завершен корректно.



admin_token = "ZXCVBNM,1234567890"# токен для администратора для авторизации всех запросов на сервер.







def verify_admin_token():
    """Проверка токена администратора"""
    auth_header = request.headers.get('Authorization')
    if not auth_header or auth_header != f"Bearer {admin_token}":
        return False
    return True
