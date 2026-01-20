# OroIdentityServerProtectedAPI

Este es un proyecto de ejemplo de ASP.NET Core Web API que está protegido por el OroIdentityServer. La API requiere autenticación JWT Bearer para acceder a sus endpoints.

## Configuración

1. Asegúrate de que el OroIdentityServer esté corriendo en `http://localhost:5160` (como en el ejemplo OroIdentityServerExample).

2. Ejecuta este proyecto API:
   ```
   dotnet run
   ```

   Por defecto, correrá en `https://localhost:5001` (o similar, verifica la salida).

## Endpoints

- `GET /weatherforecast`: Devuelve un pronóstico del tiempo. Requiere autenticación.
- `GET /userinfo`: Devuelve información del usuario autenticado, incluyendo claims. Requiere autenticación.

## Autenticación

Para acceder a los endpoints protegidos, incluye un token JWT válido en el header `Authorization: Bearer <token>`.

Obtén el token del OroIdentityServer a través de los flujos OAuth 2.0 / OpenID Connect soportados.

## Notas

- En desarrollo, `RequireHttpsMetadata` está deshabilitado para facilitar las pruebas.
- Cambia la configuración en producción para requerir HTTPS.