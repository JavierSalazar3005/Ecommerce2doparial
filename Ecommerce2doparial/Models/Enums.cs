namespace Ecommerce2doparial.Models;

public enum Role
{
    AdminRoot = 0,
    Empresa   = 1,
    Cliente   = 2
}

public enum OrderStatus
{
    Nuevo      = 0,
    Enviado    = 1,
    Entregado  = 2,
    Cancelado  = 3
}