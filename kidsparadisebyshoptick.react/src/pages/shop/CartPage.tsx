import { Link } from 'react-router-dom';
import { Trash2, ShoppingBag } from 'lucide-react';
import { useCartStore } from '@/store/cart';
import { Button } from '@/components/ui/Button';
import { formatPrice, placeholderImage, PAYMENT_POLICY } from '@/lib/utils';

export function CartPage() {
  const { items, removeItem, subTotal, totalItems } = useCartStore();

  if (items.length === 0) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center">
        <div className="text-6xl mb-4">🛒</div>
        <h1 className="text-2xl font-bold text-slate-800">Your cart is empty</h1>
        <p className="text-slate-500 mt-2 mb-6">Add some toys to get started!</p>
        <Link to="/shop"><Button><ShoppingBag className="w-4 h-4" /> Continue Shopping</Button></Link>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 py-8">
      <h1 className="text-3xl font-bold text-slate-800 mb-6">Shopping Cart ({totalItems()} items)</h1>

      <div className="space-y-4">
        {items.map((item) => (
          <div key={item.toyId} className="bg-white rounded-2xl p-4 border border-slate-100 flex gap-4 items-center">
            <img
              src={item.imageUrl || placeholderImage(item.name)}
              alt={item.name}
              className="w-20 h-20 rounded-xl object-cover bg-slate-50 shrink-0"
            />
            <div className="flex-1 min-w-0">
              <Link to={`/product/${item.toyId}`} className="font-semibold text-slate-800 hover:text-brand-600 truncate block">
                {item.name}
              </Link>
              <p className="text-brand-600 font-bold mt-1">{formatPrice(item.salePrice ?? item.price)}</p>
            </div>
            <button onClick={() => removeItem(item.toyId)} className="p-2 text-red-400 hover:text-red-600 hover:bg-red-50 rounded-lg">
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>

      <div className="bg-white rounded-2xl p-6 border border-slate-100 mt-6">
        <div className="flex justify-between text-lg font-bold text-slate-800">
          <span>Subtotal</span>
          <span>{formatPrice(subTotal())}</span>
        </div>
        <p className="text-sm text-slate-500 mt-1">Delivery charge calculated at checkout · {PAYMENT_POLICY}</p>
        <Link to="/checkout" className="block mt-4">
          <Button size="lg" className="w-full">Proceed to Checkout</Button>
        </Link>
      </div>
    </div>
  );
}
