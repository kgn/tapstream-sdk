package com.tapstream.sdk;

import com.tapstream.sdk.Hit.CompletionHandler;

class Tapstream implements Api {

	private class DelegateImpl implements Delegate {
		private int delay = 0;

		public int getDelay() {
			return delay;
		}

		public void setDelay(int delay) {
			this.delay = delay;
		}

		public boolean isRetryAllowed() {
			return false;
		}
	}

	private Delegate delegate;
	public Platform platform;
	private CoreListener listener;
	public Core core;

	public Tapstream(OperationQueue queue, String accountName, String developerSecret, Config config) {
		delegate = new DelegateImpl();
		platform = new PlatformImpl();
		listener = new CoreListenerImpl(queue);
		core = new Core(delegate, platform, listener, accountName, developerSecret, config);
		core.start();
	}

	public void setDelay(int delay) {
		delegate.setDelay(delay);
	}

	public void fireEvent(Event e) {
		core.fireEvent(e);
	}

	public void fireHit(Hit h, CompletionHandler completion) {
		core.fireHit(h, completion);
	}
}
