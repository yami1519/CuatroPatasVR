# Tienda VR - trabajo para cuidar al gato

Esta escena reemplaza el entrenamiento de boxeo por un turno sencillo en una tienda de mascotas. El cuidador trabaja preparando pedidos y recibe monedas para usar en el cuidado del gato.

## Mostrarla en Unity

1. Abre `Assets/Scenes/TiendaVR.unity`.
2. En Play, pulsa **INICIAR TURNO**.
3. El panel indica el producto pedido: comida o arena.
4. Con el visor, agarra el producto con el grip del mando y sueltalo en la bandeja azul.
5. Al completar los seis pedidos se guardan las monedas y se habilita el regreso a casa.

Para demostrarla sin visor, usa el raton sobre los productos y los botones. La escena mantiene las manos XR y la vista de raton como apoyo para explicar el flujo.

## Que puede modificar integrante 1

- En **02 - Pedidos y entrega**, selecciona el objeto principal para cambiar la lista de pedidos y el pago por pedido desde el Inspector.
- El objeto **Zona de entrega - soltar aqui** conecta la bandeja con la comprobacion del pedido.
- Los botones **INICIAR TURNO** y **VOLVER A CASA** usan eventos visibles en el Inspector.
- Los productos usan `XR Grab Interactable`: no requieren programar una nueva interaccion para agarrarse.

## Entrega de integrante 2

La parte visual puede cambiarse sin tocar la logica:

- Sustituir `Visual provisional - reemplazable` de los prefabs `Comida` y `Arena` por los modelos finales.
- Reemplazar el mostrador, panel, bandeja y suelo provisionales por el escenario de tienda.
- Conectar sonidos, particulas y animaciones a los eventos **Correct Delivery**, **Wrong Delivery** y **Shift Finished** del componente `Shop Session`.

## Siguiente integracion

El pago usa `GameManager.AddCoins`, por lo que queda guardado junto al dinero existente. Falta que el menu ofrezca la tienda y que la casa use las monedas en los objetos de cuidado ya conectados.
