export default class Collection {
  constructor(items) {
    this.items = items || [];
  }

  add(item) {
    this.items.push(item);
  }

  getAt(index) {
    return this.items[index];
  }

  remove(item) {
    const index = this.items.indexOf(item);
    if (index !== -1) {
      this.items.splice(index, 1);
    }
  }

  contains(item) {
    return this.items.includes(item);
  }

  count() {
    return this.items.length;
  }

  toArray() {
    return [...this.items];
  }

  clear() {
    this.items = [];
  }

  // Make a Range iterable by returning an iterator object.
  // Note that the name of this method is a special symbol, not a string.
  [Symbol.iterator]() {
    let items = this.items;
    let next = 0;
    let last = this.items.length - 1;
    return {
      next() {
        return next <= last ? { value: items[next++] } : { done: true };
      },
    };
  }
}
